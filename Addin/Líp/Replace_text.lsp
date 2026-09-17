;;; ==========================================================
;;; LỆNH: TEXTREPLACE
;;; Thay nội dung TEXT / MTEXT / ATTRIBUTE / LEADER / MLEADER theo mẫu chọn
;;; ==========================================================
(defun c:TEXTREPLACE (/ srcEnt srcObj srcStr ss i ent obj objName ann)

  (vl-load-com)
  (princ "\n=== LỆNH: TEXTREPLACE - Thay nội dung theo mẫu chọn ===")

  ;; --- Chọn đối tượng mẫu ---
  (setq srcEnt (car (entsel "\nChọn 1 TEXT/MTEXT/LEADER/MLEADER làm mẫu: ")))
  (if (not srcEnt)
    (progn (prompt "\n⚠️ Không chọn được đối tượng mẫu.") (exit))
  )

  (setq srcObj (vlax-ename->vla-object srcEnt))
  (setq objName (vla-get-ObjectName srcObj))

  ;; --- Lấy nội dung mẫu ---
  (cond
    ;; TEXT, MTEXT, ATTRIBUTE
    ((member objName '("AcDbText" "AcDbMText" "AcDbAttribute"))
      (setq srcStr (vla-get-TextString srcObj))
    )

    ;; LEADER có annotation
    ((= objName "AcDbLeader")
      (setq ann (vla-get-Annotation srcObj))
      (if (and ann (vlax-object-p ann))
        (setq srcStr (vla-get-TextString ann))
        (setq srcStr "")
      )
    )

    ;; MULTILEADER hoặc MLEADER
    ((or (= objName "AcDbMultiLeader")
         (= objName "AcDbMLeader"))
      (setq srcStr (vla-get-TextString srcObj))
    )

    (T
      (prompt (strcat "\n⚠️ Loại đối tượng không được hỗ trợ: " objName))
      (exit)
    )
  )

  (if (not srcStr) (setq srcStr ""))
  (princ (strcat "\n→ Nội dung mẫu: \"" srcStr "\""))

  ;; --- Chọn các đối tượng cần thay ---
  (prompt "\nChọn các TEXT / MTEXT / ATTRIBUTE / LEADER / MLEADER cần thay: ")
  (setq ss (ssget '((0 . "TEXT,MTEXT,ATTRIB,LEADER,MULTILEADER"))))

  (if (not ss)
    (prompt "\n⚠️ Không có đối tượng được chọn.")
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        (setq objName (vla-get-ObjectName obj))

        (cond
          ;; TEXT, MTEXT, ATTRIBUTE
          ((member objName '("AcDbText" "AcDbMText" "AcDbAttribute"))
            (vla-put-TextString obj srcStr)
          )

          ;; LEADER có annotation
          ((= objName "AcDbLeader")
            (setq ann (vla-get-Annotation obj))
            (if (and ann (vlax-object-p ann))
              (vla-put-TextString ann srcStr)
            )
          )

          ;; MULTILEADER hoặc MLEADER
          ((or (= objName "AcDbMultiLeader")
               (= objName "AcDbMLeader"))
            (vla-put-TextString obj srcStr)
          )
        )

        (setq i (1+ i))
      )

      (princ (strcat
        "\n✅ Đã thay " (itoa (sslength ss))
        " đối tượng bằng nội dung: \"" srcStr "\""
      ))
    )
  )

  (princ)
)
