(defun c:a1 () 
  (setvar "CLAYER" "AM 0") ; thay bằng tên layer bạn muốn
  (princ)
)

(defun c:a2 () 
  (setvar "CLAYER" "AM 3") ; thay bằng tên layer khác
  (princ)
)
(defun c:a3 () 
  (setvar "CLAYER" "AM 7") ; thay bằng tên layer khác
  (princ)
)
(defun c:a4 () 
  (setvar "CLAYER" "AM 8") ; thay bằng tên layer khác
  (princ)
)
(defun c:a5 () 
  (setvar "CLAYER" "AM 5") ; thay bằng tên layer khác
  (princ)
)
(defun c:a6 () 
  (setvar "CLAYER" "AM 6") ; thay bằng tên layer khác
  (princ)
)


(defun c:c2 () 
  (command "_.CIRCLE" "_2P")
  (princ)
)

(defun c:c3 () 
  (command "_.CIRCLE" "_D")
  (princ)
)

(defun c:P () 
  (command "_.ampowerdim")
  (princ)
)